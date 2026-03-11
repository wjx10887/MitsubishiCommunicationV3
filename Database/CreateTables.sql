-- 数据库建表脚本
-- 适用于三菱PLC通讯数据审计追踪系统

-- 创建数据库（如果不存在）
CREATE DATABASE IF NOT EXISTS plc_data_audit CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE plc_data_audit;

-- 变量配置表
CREATE TABLE IF NOT EXISTS plc_variables (
    id INT PRIMARY KEY AUTO_INCREMENT,
    address VARCHAR(50) NOT NULL COMMENT '变量地址',
    name VARCHAR(100) NOT NULL COMMENT '变量名称',
    data_type VARCHAR(20) NOT NULL COMMENT '数据类型',
    sampling_period INT NOT NULL COMMENT '采样周期(ms)',
    unit VARCHAR(20) DEFAULT '' COMMENT '工程单位',
    min_value DOUBLE DEFAULT NULL COMMENT '最小值',
    max_value DOUBLE DEFAULT NULL COMMENT '最大值',
    scale_factor DOUBLE DEFAULT 1.0 COMMENT '缩放因子',
    offset DOUBLE DEFAULT 0.0 COMMENT '偏移量',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_address (address)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='变量配置表';

-- 读取记录表
CREATE TABLE IF NOT EXISTS read_history (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    variable_id INT NOT NULL COMMENT '变量ID',
    timestamp DATETIME(3) NOT NULL COMMENT '读取时间戳(毫秒精度)',
    raw_value VARCHAR(255) NOT NULL COMMENT '原始值',
    engineering_value DOUBLE DEFAULT NULL COMMENT '工程值',
    quality_flag TINYINT NOT NULL DEFAULT 1 COMMENT '质量标志(1:良好, 0:异常)',
    FOREIGN KEY (variable_id) REFERENCES plc_variables(id) ON DELETE CASCADE,
    INDEX idx_timestamp (timestamp),
    INDEX idx_variable_timestamp (variable_id, timestamp)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='读取记录表';

-- 通讯异常记录
CREATE TABLE IF NOT EXISTS alarm_logs (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    timestamp DATETIME(3) NOT NULL COMMENT '异常时间戳(毫秒精度)',
    error_code INT NOT NULL COMMENT '错误代码',
    error_message VARCHAR(255) NOT NULL COMMENT '错误信息',
    severity TINYINT NOT NULL DEFAULT 1 COMMENT '严重程度(1:警告, 2:错误, 3:严重)',
    is_resolved TINYINT NOT NULL DEFAULT 0 COMMENT '是否已解决(0:未解决, 1:已解决)',
    resolved_at DATETIME DEFAULT NULL COMMENT '解决时间',
    INDEX idx_timestamp (timestamp),
    INDEX idx_severity (severity)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='通讯异常记录';

-- 历史数据表（用于数据归档）
CREATE TABLE IF NOT EXISTS read_history_archive (
    id BIGINT PRIMARY KEY,
    variable_id INT NOT NULL,
    timestamp DATETIME(3) NOT NULL,
    raw_value VARCHAR(255) NOT NULL,
    engineering_value DOUBLE DEFAULT NULL,
    quality_flag TINYINT NOT NULL DEFAULT 1,
    archived_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT '归档时间',
    INDEX idx_timestamp (timestamp),
    INDEX idx_variable_timestamp (variable_id, timestamp)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='读取记录归档表';

-- 创建存储过程：归档数据
DELIMITER //
CREATE PROCEDURE archive_old_data(IN days_to_keep INT)
BEGIN
    -- 将指定天数前的数据归档到历史表
    INSERT INTO read_history_archive (id, variable_id, timestamp, raw_value, engineering_value, quality_flag)
    SELECT id, variable_id, timestamp, raw_value, engineering_value, quality_flag
    FROM read_history
    WHERE timestamp < DATE_SUB(NOW(), INTERVAL days_to_keep DAY);
    
    -- 删除已归档的数据
    DELETE FROM read_history
    WHERE timestamp < DATE_SUB(NOW(), INTERVAL days_to_keep DAY);
END //
DELIMITER ;

-- 创建索引优化查询性能
CREATE INDEX idx_plc_variables_sampling_period ON plc_variables(sampling_period);
CREATE INDEX idx_read_history_quality_flag ON read_history(quality_flag);
